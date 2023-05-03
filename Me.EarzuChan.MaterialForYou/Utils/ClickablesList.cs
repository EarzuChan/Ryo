using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Me.EarzuChan.MaterialForYou.Utils
{
    public class ManagedClickablesList : IList<UIElement>
    {

        private readonly List<UIElement> list = new();
        private readonly UIManager manager;

        public ManagedClickablesList(UIManager givenManager)
        {
            manager = givenManager ?? throw new NoNullAllowedException("必须为可点击列表绑定UI管理器");
        }

        public int Count => list.Count;

        public bool IsReadOnly => false;

        public UIElement this[int index] { get => list[index]; set => throw new NotImplementedException(); }

        public int IndexOf(UIElement item) => list.IndexOf(item);

        public void Insert(int index, UIElement item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(UIElement item)
        {
            manager.BindClickable(item);

            list.Add(item);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(UIElement item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(UIElement[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(UIElement item)
        {
            if (!list.Contains(item)) throw new IndexOutOfRangeException(item + "未被添加到本可点击列表");

            manager.UnbindClickable(item);
            list.Remove(item);

            return true;
        }

        public IEnumerator<UIElement> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
