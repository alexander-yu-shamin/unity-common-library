using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Serialization;

namespace Common
{
public class SerializableDictionary
{
}

[Serializable]
public class SerializableDictionary<TKey, TValue> : SerializableDictionary,
                                                    ISerializationCallbackReceiver,
                                                    IDictionary<TKey, TValue>,
                                                    IEquatable<SerializableDictionary<TKey, TValue>>
{
  [SerializeField]
  private List<SerializableKeyValuePair> list = new List<SerializableKeyValuePair>();

  [Serializable]
  public class SerializableKeyValuePair : IEquatable<SerializableKeyValuePair>
  {
    public TKey Key;
    public TValue Value;

    public SerializableKeyValuePair(TKey key, TValue value)
    {
      Key = key;
      Value = value;
    }

    public void SetValue(TValue value) { Value = value; }

#region EqualsAndIEquatable
    public bool Equals(SerializableKeyValuePair others)
    {
      if (others is null)
      {
        return false;
      }

      if (object.ReferenceEquals(this, others))
      {
        return true;
      }

      if (this.GetType() != others.GetType())
      {
        return false;
      }
      return Key.Equals(others.Key) && Value.Equals(others.Value);
    }

    public override bool Equals(object obj) => this.Equals(obj as SerializableKeyValuePair);

    public override int GetHashCode() => throw new System.NotImplementedException();

    public static bool operator ==(SerializableKeyValuePair lhs, SerializableKeyValuePair rhs)
    {
      if (lhs is null) return rhs is null;
      return lhs.Equals(rhs);
    }

    public static bool operator !=(SerializableKeyValuePair lhs,
                                   SerializableKeyValuePair rhs) => !(lhs == rhs);
#endregion
  }

  private Dictionary<TKey, uint> KeyPositions => _keyPositions.Value;
  private Lazy<Dictionary<TKey, uint>> _keyPositions;

  public SerializableDictionary()
  {
    _keyPositions = new Lazy<Dictionary<TKey, uint>>(MakeKeyPositions);
  }

  private Dictionary<TKey, uint> MakeKeyPositions()
  {
    var numEntries = list.Count;
    var result = new Dictionary<TKey, uint>(numEntries);
    for (int i = 0; i < numEntries; i++) result[list[i].Key] = (uint)i;
    return result;
  }

  public void OnBeforeSerialize() {}
  public void OnAfterDeserialize()
  {
    // After deserialization, the key positions might be changed
    _keyPositions = new Lazy<Dictionary<TKey, uint>>(MakeKeyPositions);
  }

#region IDictionary < TKey, TValue>
  public TValue this[TKey key]
  {
    get => list[(int)KeyPositions[key]].Value;
    set
    {
      if (KeyPositions.TryGetValue(key, out uint index))
        list[(int)index].SetValue(value);
      else
      {
        KeyPositions[key] = (uint)list.Count;
        list.Add(new SerializableKeyValuePair(key, value));
      }
    }
  }

  public ICollection<TKey> Keys => list.Select(tuple => tuple.Key).ToArray();
  public ICollection<TValue> Values => list.Select(tuple => tuple.Value).ToArray();

  public void Add(TKey key, TValue value)
  {
    if (KeyPositions.ContainsKey(key))
      throw new ArgumentException("An element with the same key already exists in the dictionary.");
    else
    {
      KeyPositions[key] = (uint)list.Count;
      list.Add(new SerializableKeyValuePair(key, value));
    }
  }

  public bool ContainsKey(TKey key) => KeyPositions.ContainsKey(key);

  public bool Remove(TKey key)
  {
    if (KeyPositions.TryGetValue(key, out uint index))
    {
      var kp = KeyPositions;
      kp.Remove(key);

      var numEntries = list.Count;

      list.RemoveAt((int)index);
      for (uint i = index; i < numEntries; i++) kp[list[(int)i].Key] = i;

      return true;
    }
    else
      return false;
  }

  public bool TryGetValue(TKey key, out TValue value)
  {
    if (KeyPositions.TryGetValue(key, out uint index))
    {
      value = list[(int)index].Value;
      return true;
    }
    else
    {
      value = default;
      return false;
    }
  }
#endregion

#region ICollection < KeyValuePair < TKey, TValue>>
  public int Count => list.Count;
  public bool IsReadOnly => false;

  public void Add(KeyValuePair<TKey, TValue> kvp) => Add(kvp.Key, kvp.Value);

  public void Clear() => list.Clear();
  public bool Contains(KeyValuePair<TKey, TValue> kvp) => KeyPositions.ContainsKey(kvp.Key);

  public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
  {
    var numKeys = list.Count;
    if (array.Length - arrayIndex < numKeys) throw new ArgumentException("arrayIndex");
    for (int i = 0; i < numKeys; i++, arrayIndex++)
    {
      var entry = list[i];
      array[arrayIndex] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
    }
  }

  public bool Remove(KeyValuePair<TKey, TValue> kvp) => Remove(kvp.Key);
#endregion

#region IEnumerable < KeyValuePair < TKey, TValue>>
  public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
  {
    return list.Select(ToKeyValuePair).GetEnumerator();

    KeyValuePair<TKey, TValue> ToKeyValuePair(SerializableKeyValuePair skvp)
    {
      return new KeyValuePair<TKey, TValue>(skvp.Key, skvp.Value);
    }
  }
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#endregion

#region EqualsAndIEquatable
  public bool Equals(SerializableDictionary<TKey, TValue> others)
  {
    if (others is null)
    {
      return false;
    }

    if (object.ReferenceEquals(this, others))
    {
      return true;
    }

    if (this.GetType() != others.GetType())
    {
      return false;
    }

    return list.SequenceEqual(others.list);
  }

  public override bool Equals(object obj) =>
    this.Equals(obj as SerializableDictionary<TKey, TValue>);

  public override int GetHashCode() => throw new System.NotImplementedException();

  public static bool operator ==(SerializableDictionary<TKey, TValue> lhs,
                                 SerializableDictionary<TKey, TValue> rhs)
  {
    if (lhs is null) return rhs is null;
    return lhs.Equals(rhs);
  }

  public static bool operator !=(SerializableDictionary<TKey, TValue> lhs,
                                 SerializableDictionary<TKey, TValue> rhs) => !(lhs == rhs);
#endregion
}
}  // namespace