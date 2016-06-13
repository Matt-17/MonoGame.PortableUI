using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public class ControlCollection : IList<Control>
    {
        private readonly List<Control> _controls;
        private readonly Panel _parent;

        public ControlCollection(Panel panel)
        {
            _controls = new List<Control>();
            _parent = panel;
        }

        public IEnumerator<Control> GetEnumerator()
        {
            return _controls.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Control item)
        {
            SetParent(item);
            _controls.Add(item);
            OnElementsAdded(item);
        }

        public void Clear()
        {
            var controls = _controls.ToArray();
            _controls.Clear();
            OnElementsRemoved(controls);
        }

        public bool Contains(Control item)
        {
            return _controls.Contains(item);
        }

        public void CopyTo(Control[] array, int arrayIndex)
        {
            _controls.CopyTo(array, arrayIndex);
        }

        public bool Remove(Control item)
        {
            UnsetParent(item);
            var remove = _controls.Remove(item);
            OnElementsRemoved(item);
            return remove;
        }

        public int Count
        {
            get { return _controls.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(Control item)
        {
            return _controls.IndexOf(item);
        }

        public void Insert(int index, Control item)
        {
            SetParent(item);
            _controls.Insert(index, item);
            OnElementsAdded(item);
        }

        public void RemoveAt(int index)
        {
            var control = _controls[index];
            UnsetParent(control);
            _controls.RemoveAt(index);
            OnElementsRemoved(control);
        }

        public Control this[int index]
        {
            get { return _controls[index]; }
            set
            {
                var removed = _controls[index];
                var added = value;
                if (removed == added)
                    return;
                UnsetParent(removed);
                SetParent(added);
                _controls[index] = added;
                OnElementChanged(added, removed);
            }
        }

        protected virtual void OnElementsAdded(params Control[] control)
        {
            CollectionChanged?.Invoke(this, new CollectionChangedEventArgs(control, null));
        }

        protected virtual void OnElementsRemoved(params Control[] control)
        {
            CollectionChanged?.Invoke(this, new CollectionChangedEventArgs(null, control));
        }

        protected virtual void OnElementChanged(Control added, Control removed)
        {
            CollectionChanged?.Invoke(this, new CollectionChangedEventArgs(new[] {added}, new[] {removed}));
        }

        public event CollectionChangedEvent CollectionChanged;

        protected virtual void OnCollectionChanged(CollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }

        private void UnsetParent(Control item)
        {
            item.Parent = null;
        }

        private void SetParent(Control item)
        {
            item.Parent = _parent;
        }
    }
}