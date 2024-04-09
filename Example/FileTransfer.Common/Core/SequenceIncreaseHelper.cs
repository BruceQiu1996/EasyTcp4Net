namespace FileTransfer.Common.Core
{
    public class SequenceIncreaseHelper
    {
        public SequenceIncreaseHelper() { }

        private static readonly object _lock = new object();
        private static long _sequenceId = 0;

        public static long Next()
        {
            lock (_lock)
            {
                return ++_sequenceId;
            }
        }
    }
}
