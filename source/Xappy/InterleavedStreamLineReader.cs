using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xappy
{
    public class NoStreamsProvidedException: Exception
    {
        public NoStreamsProvidedException()
            : base("You have not provded any streams to read interleaved from")
        {
        }
    }
    public class InterleavedStreamLineReader: IDisposable
    {
        private List<StreamReader> _readers;
        private List<string> _readLines;
        private List<Task> _tasks;

        public InterleavedStreamLineReader()
        {
            throw new NoStreamsProvidedException();
        }
        public InterleavedStreamLineReader(params Stream[] streams)
        {
            if (streams == null || streams.Length == 0)
                throw new NoStreamsProvidedException();
            _readLines = new List<string>();
            _readers = new List<StreamReader>();
            foreach (var stream in streams)
                _readers.Add(new StreamReader(stream));
            ObserveStreams();
        }

        private void ObserveStreams()
        {
            _tasks = new List<Task>();
            foreach (var reader in _readers)
            {
                var readTask = Task.Run(() =>
                    {
                        while (true)
                        {
                            if (!ReaderIsValid(reader)) break;
                            if (!ReaderIsReady(reader)) continue;
   
                            var thisLine = reader.ReadLine();
                            AddLineToReadLines(thisLine);
                        }
                    });
                _tasks.Add(readTask);
            }
        }

        private bool ReaderIsReady(StreamReader reader)
        {
            try
            {
                return reader.Peek() >= 0;
            }
            catch
            {
                return false;
            }
        }

        private bool ReaderIsValid(StreamReader reader)
        {
            try
            {
                reader.Peek();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void AddLineToReadLines(string line)
        {
            lock (_readLines)
            {
                _readLines.Add(line);
            }
        }

        public string[] GetInterleavedOutput()
        {
            lock (_readLines)
            {
                return _readLines.ToArray();
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                foreach (var task in _tasks)
                    task.Wait();
                DisposeOfDisposables(_tasks.Select(t => t as IDisposable));
                _tasks.Clear();
                DisposeOfDisposables(_readers.Select(r => r as IDisposable));
                _readers.Clear();
            }
        }

        private void DisposeOfDisposables(IEnumerable<IDisposable> disposables)
        {
            var enumerable = disposables as IDisposable[] ?? disposables.ToArray();
            if (disposables == null || !enumerable.Any()) return;
            foreach (var disposable in enumerable)
            {
                if (disposable != null)
                    disposable.Dispose();
            }
        }
    }
}
