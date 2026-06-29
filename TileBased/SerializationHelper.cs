namespace TileBased;

public static class SerializationHelper {
    public const int SIZEOF_BYTE = 8;
    public static unsafe int bitsizeof<T>() where T : unmanaged => sizeof(T) * SIZEOF_BYTE;

    public static ulong MakeMask(int width, int position) {
        ulong mask = 0;
        for (int i = 0; i < width; i++) {
            mask <<= 1;
            mask |= 1;
        }
        mask <<= position;

        return mask;
    }
}