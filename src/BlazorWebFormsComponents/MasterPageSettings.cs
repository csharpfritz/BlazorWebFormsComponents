using System;
using System.Collections;
using System.Collections.Generic;

namespace BlazorWebFormsComponents
{
	public class MasterPageSettings : IDictionary<string, object>
	{

		private Dictionary<string, object> _Internal = new Dictionary<string, object>();

		internal Action StateHasChanged { get; set; }

		public void Reload() => StateHasChanged?.Invoke();

		public object this[string key] {
			get { return _Internal[key]; }
			set {
				_Internal[key] = value;
				StateHasChanged?.Invoke();
			}
		}

		public ICollection<string> Keys => _Internal.Keys;
		public ICollection<object> Values => _Internal.Values; 
		public int Count => _Internal.Count; 
		public bool IsReadOnly => false; 

		public void Add(string key, object value)
		{

			_Internal.Add(key, value);
			StateHasChanged?.Invoke();

		}

		public void Add(KeyValuePair<string, object> item)
		{

			_Internal.Add(item.Key, item.Value);
			StateHasChanged?.Invoke();

		}

		public void Clear() =>_Internal.Clear();

		public bool Contains(KeyValuePair<string, object> item) =>
			_Internal.ContainsKey(item.Key) && _Internal[item.Key] == item.Value;

		public bool ContainsKey(string key) => _Internal.ContainsKey(key);

		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _Internal.GetEnumerator();

		public bool Remove(string key)
		{
			var removed = _Internal.Remove(key);
			if (removed) StateHasChanged?.Invoke();
			return removed;
		}

		public bool Remove(KeyValuePair<string, object> item) => Remove(item.Key);

		public bool TryGetValue(string key, out object value) => _Internal.TryGetValue(key, out value);

		IEnumerator IEnumerable.GetEnumerator() => _Internal.GetEnumerator();

	}

}
