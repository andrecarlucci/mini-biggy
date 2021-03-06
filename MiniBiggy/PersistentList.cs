﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiniBiggy.SaveStrategies;
using Newtonsoft.Json;

namespace MiniBiggy {

    public class PersistentList<T> : ICollection<T> where T : new() {

        private readonly List<T> _items;
        private readonly IDataStore _dataStore;
        public ISaveStrategy SaveStrategy { get; }

        public event EventHandler<PersistedEventArgs<T>> ItemsRemoved;
        public event EventHandler<PersistedEventArgs<T>> ItemsAdded;
        public event EventHandler<PersistedEventArgs<T>> ItemsUpdated;
        public event EventHandler<PersistedEventArgs<T>> ItemsChanged;
        public event EventHandler Loaded;
        public event EventHandler Saved;

        public bool IsNew { get; set; }

        public PersistentList(IDataStore dataStore, ISaveStrategy saveStrategy) {
            _dataStore = dataStore;
            _items = new List<T>();
            if (saveStrategy == null) {
                saveStrategy = new SaveOnlyWhenRequested();
            }
            SaveStrategy = saveStrategy;
            SaveStrategy.NotifyUnsolicitedSave += (sender, args) => Save();
            Load();
        }

        private void Load() {
            var json = _dataStore.ReadAllTextAsync(Name).Result;
            if (String.IsNullOrEmpty(json)) {
                IsNew = true;
                return;
            }
            _items.AddRange(JsonConvert.DeserializeObject<List<T>>(json));
        }
        
        public virtual string Name {
            get {
                return typeof(T).Name;
            }
        }

        public void Save() {
            SaveAsync().Wait();
            OnSaved();
        }

        public async Task SaveAsync() {
            var json = JsonConvert.SerializeObject(_items);
            await _dataStore.WriteAllTextAsync(Name, json);
        }

        public async virtual Task<int> UpdateAsync(T item) {
            var index = _items.IndexOf(item);
            if (index > -1) {
                _items.RemoveAt(index);
                _items.Insert(index, item);
            }
            if (SaveStrategy.ShouldSaveNow()) {
                await SaveAsync();
            }
            OnItemsUpdated(new List<T> { item });
            OnItemsChanged(new List<T> { item });
            return 1;
        }

        public async virtual Task<int> UpdateAsync(IEnumerable<T> items) {
            var itemsToUpdate = items.ToList();
            foreach (var item in itemsToUpdate) {
                var index = _items.IndexOf(item);
                if (index > -1) {
                    _items.RemoveAt(index);
                    _items.Insert(index, item);
                }
            }
            if (SaveStrategy.ShouldSaveNow()) {
                await SaveAsync();
            }
            OnItemsUpdated(itemsToUpdate);
            OnItemsChanged(itemsToUpdate);
            return itemsToUpdate.Count();
        }

        public virtual void Add(T item) {
            _items.Add(item);
            if (SaveStrategy.ShouldSaveNow()) {
                Save();
            }
            OnItemsAdded(new List<T> { item });
            OnItemsChanged(new List<T> { item });
        }

        public void Add(IEnumerable<T> items) {
            var list = items.ToList();
            _items.AddRange(list);
            if (SaveStrategy.ShouldSaveNow()) {
                Save();
            }
            OnItemsAdded(list);
            OnItemsChanged(list);
        }

        public virtual void Clear() {
            _items.Clear();
            if (SaveStrategy.ShouldSaveNow()) {
                Save();
            }
            OnItemsChanged(new List<T>());
        }

        public virtual bool Contains(T item) {
            return _items.Contains(item);
        }

        public virtual void CopyTo(T[] array, int arrayIndex) {
            _items.CopyTo(array, arrayIndex);
        }

        public virtual int Count {
            get { return _items.Count; }
        }

        public virtual bool IsReadOnly {
            get { return false; }
        }

        public virtual bool Remove(T item) {
            var removed = _items.Remove(item);
            if (SaveStrategy.ShouldSaveNow()) {
                Save();
            }
            OnItemsRemoved(new List<T> { item });
            OnItemsChanged(new List<T> { item });
            return removed;
        }

        public virtual int Remove(IEnumerable<T> items) {
            var itemsToRemove = items.ToList();
            var removedItems = new List<T>();
            foreach (var item in itemsToRemove) {
                if (_items.Remove(item)) {
                    removedItems.Add(item);
                }
            }
            if (SaveStrategy.ShouldSaveNow()) {
                Save();
            }
            OnItemsRemoved(removedItems);
            OnItemsChanged(removedItems);
            return removedItems.Count();
        }

        public IEnumerator<T> GetEnumerator() {
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _items.GetEnumerator();
        }

        protected virtual void OnItemsRemoved(List<T> items) {
            ItemsRemoved?.Invoke(this, new PersistedEventArgs<T>(items));
        }

        protected virtual void OnItemsUpdated(List<T> items) {
            ItemsUpdated?.Invoke(this, new PersistedEventArgs<T>(items));
        }

        protected virtual void OnItemsChanged(List<T> items) {
            ItemsChanged?.Invoke(this, new PersistedEventArgs<T>(items));
        }

        protected virtual void OnItemsAdded(List<T> items) {
            ItemsAdded?.Invoke(this, new PersistedEventArgs<T>(items));
        }

        protected virtual void OnLoaded() {
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnSaved() {
            Saved?.Invoke(this, EventArgs.Empty);
        }
    }
}