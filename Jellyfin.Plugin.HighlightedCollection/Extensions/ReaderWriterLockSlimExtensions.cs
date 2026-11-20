using System;
using System.Threading;

namespace Jellyfin.Plugin.HighlightedCollection.Extensions;

/// <summary>
/// Defines extensions to ease the use of the <see cref="ReaderWriterLockSlim"/> class.
/// </summary>
public static class ReaderWriterLockSlimExtensions
{
    /// <summary>
    /// Wraps the provided function in a read lock.
    /// </summary>
    /// <param name="rwls">The <see cref="ReaderWriterLockSlim"/> being used.</param>
    /// <param name="readOperation">The read operation.</param>
    /// <typeparam name="TResult">Result returned by readOperation.</typeparam>
    /// <returns>TResult.</returns>
    public static TResult SafeRead<TResult>(this ReaderWriterLockSlim rwls, Func<TResult> readOperation)
    {
        rwls = rwls ?? throw new ArgumentNullException(nameof(rwls));
        readOperation = readOperation ?? throw new ArgumentNullException(nameof(readOperation));
        try
        {
            rwls.EnterReadLock();
            return readOperation();
        }
        finally
        {
            rwls.ExitReadLock();
        }
    }

    /// <summary>
    /// Wraps the provided action in a write lock.
    /// </summary>
    /// <param name="rwls">The <see cref="ReaderWriterLockSlim"/> being used.</param>
    /// <param name="writeOperation">The write operation.</param>
    public static void SafeWrite(this ReaderWriterLockSlim rwls, Action writeOperation)
    {
        rwls = rwls ?? throw new ArgumentNullException(nameof(rwls));
        writeOperation = writeOperation ?? throw new ArgumentNullException(nameof(writeOperation));
        try
        {
            rwls.EnterWriteLock();
            writeOperation();
        }
        finally
        {
            rwls.ExitWriteLock();
        }
    }
}
