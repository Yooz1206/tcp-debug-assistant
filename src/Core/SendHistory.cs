using System;
using System.Collections.Generic;

namespace TcpDebug.Core
{
    /// <summary>
    /// 发送历史，支持用上下键回看最近发送过的内容。
    /// </summary>
    public sealed class SendHistory
    {
        private readonly List<string> _items = new List<string>();
        private int _cursor;

        public SendHistory() : this(100)
        {
        }

        public SendHistory(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException("capacity", "容量至少为 1。");
            }

            Capacity = capacity;
        }

        public int Capacity { get; private set; }

        public IReadOnlyList<string> Items
        {
            get { return _items; }
        }

        public void Add(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (_items.Count > 0 && _items[_items.Count - 1] == text)
            {
                Reset();
                return;
            }

            _items.Add(text);
            while (_items.Count > Capacity)
            {
                _items.RemoveAt(0);
            }

            Reset();
        }

        public string Previous()
        {
            if (_cursor <= 0)
            {
                return null;
            }

            _cursor--;
            return _items[_cursor];
        }

        public string Next()
        {
            if (_cursor >= _items.Count)
            {
                return null;
            }

            _cursor++;
            return _cursor >= _items.Count ? null : _items[_cursor];
        }

        public void Reset()
        {
            _cursor = _items.Count;
        }
    }
}
