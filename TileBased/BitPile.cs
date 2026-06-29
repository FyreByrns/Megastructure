using System.Text;
using static TileBased.SerializationHelper;

namespace TileBased;

/// <summary>
/// A helper class for turning stuff into bits.
/// </summary>
public class BitPile {
    // store the bits packed into a backing type
    ulong[] backingData = new ulong[1];
    /// <summary> Backing data bit size. </summary>
    static readonly int _backingDataBitWidth = bitsizeof<ulong>();
    int currentBackingPointer = 0; // "read / write head" to the current backing block
    int currentBitPointer = 0; // "read / write head" to a specific bit in the backing block

    /// <summary>
    /// Ensure the backing pointer and backing bit pointer are sensible:
    /// expand the backing data array if the bit pointer is greater than the backing data size
    /// and maintain the bit pointer offset
    /// </summary>
    void MaintainBackingArray() {
        if (currentBitPointer >= _backingDataBitWidth) {
            currentBackingPointer++;
            currentBitPointer -= _backingDataBitWidth;
        }

        if (currentBackingPointer >= backingData.Length) {
            int newArraySize = backingData.Length * 2;
            Array.Resize(ref backingData, newArraySize);
        }
    }

    public int TotalByteSize => backingData.Length * _backingDataBitWidth;
    public int TotalBitSize => ((backingData.Length - 1) * _backingDataBitWidth) + currentBitPointer;

    /// <summary>
    /// Get the index of the backing ulong which contains the specific bit index.
    /// </summary>
    int GetBackingIndex(int bitIndex) {
        int backingIndex = bitIndex / _backingDataBitWidth;
        return backingIndex;
    }
    /// <summary>
    /// Get the actual bit position within the backing ulong.
    /// </summary>
    int GetBackingSubIndex(int bitIndex) {
        int backingIndex = GetBackingIndex(bitIndex);
        int subIndex = bitIndex % _backingDataBitWidth;
        return subIndex;
    }

    public void Write(bool b) {
        if (b) { backingData[currentBackingPointer] |= (1ul << currentBitPointer); }
        currentBitPointer++;
        MaintainBackingArray();
    }
    public void Write<T>(T num, int specificBitWidth = 0) where T : unmanaged {
        int _bitWidth = specificBitWidth == 0 ? bitsizeof<T>() : specificBitWidth;
        ulong mask = MakeMask(currentBitPointer + _bitWidth, 0);
        int newBitPointer = currentBitPointer + _bitWidth;
        int sizeOverrun = newBitPointer - _backingDataBitWidth;

        //ulong d = (ulong)Convert.ToInt64(num);
        // ^^ the "proper" way, with bounds testing etc.
        // vv ugly bit-level hack.  I'm as smart as John Carmack.
        ulong d;
        unsafe {
            d = *(ulong*)&num;
        }

        if (sizeOverrun > 0) { // data will overrun the current backing ulong
            // write the portion that can fit in the current ulong
            ulong inThis = d << currentBitPointer;
            backingData[currentBackingPointer] |= inThis;
            currentBitPointer = _backingDataBitWidth;
            // move to the next
            MaintainBackingArray();

            // write the rest
            ulong inNext = d >> (_bitWidth - sizeOverrun);
            backingData[currentBackingPointer] |= inNext;
            currentBitPointer += sizeOverrun;

            // ensure the set didn't corrupt future bits due to negative integers
            mask = MakeMask(sizeOverrun, 0);
            backingData[currentBackingPointer] &= mask;

            return;
        }

        // otherwise, just write the data
        // position value
        d <<= currentBitPointer;
        // set
        backingData[currentBackingPointer] |= d;

        // ensure the set didn't corrupt future bits due to negative integers
        backingData[currentBackingPointer] &= mask;

        currentBitPointer += _bitWidth;
        MaintainBackingArray();
    }

    public bool Read(int bitIndex) {
        int backingIndex = GetBackingIndex(bitIndex);
        if (backingIndex >= backingData.Length) { return false; }

        ulong mask = MakeMask(1, bitIndex % _backingDataBitWidth);

        return (backingData[backingIndex] & mask) > 0;
    }
    public T Read<T>(int bitIndex, int specificBitWidth = 0) where T : unmanaged {
        int _bitWidth = specificBitWidth == 0 ? bitsizeof<T>() : specificBitWidth;
        ulong slice = GetSlice(bitIndex, bitIndex + _bitWidth);

        //ulong maxValue = Type.GetTypeCode(typeof(T)) switch {
        //    TypeCode.Byte => byte.MaxValue,
        //    TypeCode.SByte => sbyte.MaxValue,

        //    _ => ulong.MaxValue
        //};

        //if (slice > maxValue) { slice = maxValue; }
        //return (T)Convert.ChangeType(slice, typeof(T));

        // ^^ the "proper" way, with bounds testing etc.
        // vv ugly bit-level hack.  I'm as smart as John Carmack.
        unsafe {
            T v = *(T*)&slice;
            return v;
        }
    }
    public bool ReadAndAdvance(ref int bitIndex) {
        bool result = Read(bitIndex);
        bitIndex++;
        return result;
    }
    public T ReadAndAdvance<T>(ref int bitIndex, int specificBitWidth = 0) where T : unmanaged {
        T result = Read<T>(bitIndex, specificBitWidth);
        bitIndex += specificBitWidth == 0 ? bitsizeof<T>() : specificBitWidth;
        return result;
    }

    public ulong GetSlice(int bitIndexStart, int bitIndexEnd) {
        if (bitIndexStart > bitIndexEnd) {
            (bitIndexStart, bitIndexEnd) = (bitIndexEnd, bitIndexStart);
        }
        int sliceWidth = bitIndexEnd - bitIndexStart;

        int backingBitIndexStart = GetBackingSubIndex(bitIndexStart);
        int backingBitIndexEnd = GetBackingSubIndex(bitIndexEnd);
        int backingIndexStart = GetBackingIndex(bitIndexStart);
        int backingIndexEnd = GetBackingIndex(bitIndexEnd);

        if (backingIndexStart != backingIndexEnd) { // slice crosses a backing boundary
            int bitWidthInFirst = _backingDataBitWidth - backingBitIndexStart;
            int bitWidthInSecond = sliceWidth - bitWidthInFirst;

            ulong inFirstBacking = backingData[backingIndexStart];
            inFirstBacking &= MakeMask(bitWidthInFirst, backingBitIndexStart);
            inFirstBacking >>= backingBitIndexStart; // shift to zero offset

            ulong inSecondBacking = backingData[backingIndexEnd];
            inSecondBacking &= MakeMask(bitWidthInSecond, 0);
            inSecondBacking <<= bitWidthInFirst; // shift to correct spot

            return inFirstBacking | inSecondBacking;
        }

        // slice doesn't cross a backing boundary
        return (
            backingData[backingIndexStart]
            & MakeMask(sliceWidth, backingBitIndexStart)) // mask out other data
            >> backingBitIndexStart; // shift to zero offset
    }

    public IEnumerable<byte> Bytes() {
        for (int i = 0; i < backingData.Length; i++) {
            for (int byteIndex = 0; byteIndex < sizeof(ulong); byteIndex++) {
                byte b = (byte)(backingData[i] >> byteIndex * bitsizeof<byte>());
                yield return b;
            }
        }
    }

    public BitPile() { }
    public BitPile(byte[] bytes) {
        int backingDataSize = bytes.Length / sizeof(ulong);
        backingData = new ulong[backingDataSize];

        for (int i = 0; i < backingData.Length; i++) {
            for (int b = 0; b < 8; b++) {
                ulong v = bytes[i * 8 + b];
                v <<= b * 8;
                backingData[i] |= v;
            }
        }
    }

    public override string ToString() {
        StringBuilder sb = new();

        sb.AppendLine("1111111122222222333333334444444455555555666666667777777788888888 <-{byte");
        sb.AppendLine("_________10________20________30________40________50________60___ <-{bit index");
        sb.AppendLine("123456789^123456789^123456789^123456789^123456789^123456789^1234 v-{backing array index");

        int backingIndex = 0;
        foreach (ulong backing in backingData) {
            sb.Append(new string([.. Convert.ToString((long)backing, 2).PadLeft(_backingDataBitWidth, '0').Reverse()]));
            sb.AppendLine($" {backingIndex}");
            if (backingIndex == currentBackingPointer) {
                sb.Append([.. "^".PadRight(currentBitPointer + 1).PadLeft(_backingDataBitWidth, ' ').Reverse()]);
                sb.AppendLine("  <-{current write head");
            }
            backingIndex++;
        }

        return sb.ToString();
    }
}
