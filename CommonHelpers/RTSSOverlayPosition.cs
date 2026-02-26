using System.IO.MemoryMappedFiles;

namespace CommonHelpers
{
    public static class RTSSOverlayPosition
    {
        private const string SharedMemoryName = "RTSSSharedMemoryV2";
        // Accept both byte-order variants for safety across readers/writers.
        private const uint SharedMemorySignature = 0x53535452; // 'RTSS'
        private const uint SharedMemorySignatureAlt = 0x52545353; // 'RTSS'
        private const uint MinSharedMemoryVersion = 0x00020000; // v2.0

        private const long SignatureOffset = 0;
        private const long VersionOffset = 4;
        private const long AppEntrySizeOffset = 8;
        private const long AppArrayOffsetOffset = 12;
        private const long AppArraySizeOffset = 16;
        private const long GlobalOsdFrameOffset = 32;

        // Offsets from RTSS_SHARED_MEMORY_APP_ENTRY in RTSSSharedMemory.h.
        private const int AppEntryProcessIdOffset = 0;
        private const int AppEntryOsdXOffset = 320;
        private const int AppEntryOsdYOffset = 324;

        public static bool TrySetCoordinates(int x, int y)
        {
            try
            {
                using var mmf = MemoryMappedFile.OpenExisting(SharedMemoryName, MemoryMappedFileRights.ReadWrite);
                using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);

                if (accessor.Capacity <= GlobalOsdFrameOffset)
                    return false;

                var signature = accessor.ReadUInt32(SignatureOffset);
                var version = accessor.ReadUInt32(VersionOffset);
                if ((signature != SharedMemorySignature && signature != SharedMemorySignatureAlt) || version < MinSharedMemoryVersion)
                    return false;

                var appEntrySize = accessor.ReadUInt32(AppEntrySizeOffset);
                var appArrayOffset = accessor.ReadUInt32(AppArrayOffsetOffset);
                var appArraySize = accessor.ReadUInt32(AppArraySizeOffset);

                if (appEntrySize < AppEntryOsdYOffset + sizeof(int))
                    return false;

                bool changed = false;
                for (uint i = 0; i < appArraySize; i++)
                {
                    long entryOffset = appArrayOffset + (long)i * appEntrySize;
                    long entryEnd = entryOffset + AppEntryOsdYOffset + sizeof(int);
                    if (entryOffset < 0 || entryEnd > accessor.Capacity)
                        break;

                    if (accessor.ReadInt32(entryOffset + AppEntryProcessIdOffset) == 0)
                        continue;

                    var currentX = accessor.ReadInt32(entryOffset + AppEntryOsdXOffset);
                    var currentY = accessor.ReadInt32(entryOffset + AppEntryOsdYOffset);
                    if (currentX == x && currentY == y)
                        continue;

                    accessor.Write(entryOffset + AppEntryOsdXOffset, x);
                    accessor.Write(entryOffset + AppEntryOsdYOffset, y);
                    changed = true;
                }

                if (changed)
                {
                    var frame = accessor.ReadUInt32(GlobalOsdFrameOffset);
                    accessor.Write(GlobalOsdFrameOffset, unchecked(frame + 1));
                }

                return changed;
            }
            catch
            {
                return false;
            }
        }
    }
}
