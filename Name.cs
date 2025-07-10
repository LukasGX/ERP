using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Fix
{
    public class NameEntry<TId, TName, TValue>
    {
        public TId Id { get; set; }
        public TName Name { get; set; }
        public TValue Value { get; set; }

        public NameEntry(TId id, TName name, TValue value)
        {
            Id = id;
            Name = name;
            Value = value;
        }
    }

    public class NameCollection<TId, TName, TValue>
    {
        private List<NameEntry<TId, TName, TValue>> entries = new List<NameEntry<TId, TName, TValue>>();
        public int Count => entries.Count;

        public void Add(TId id, TName name, TValue value)
        {
            if (ContainsId(id))
                throw new ArgumentException($"Id '{id}' already exists."); // Improve error message
            if (ContainsName(name))
                throw new ArgumentException($"Name '{name}' already exists."); // Improve error message

            entries.Add(new NameEntry<TId, TName, TValue>(id, name, value));
        }

        public bool ContainsId(TId id)
        {
            return entries.Any(e => EqualityComparer<TId>.Default.Equals(e.Id, id));
        }

        public bool ContainsName(TName name)
        {
            return entries.Any(e => EqualityComparer<TName>.Default.Equals(e.Name, name));
        }

        public NameEntry<TId, TName, TValue>? GetById(TId id)
        {
            return entries.FirstOrDefault(e => EqualityComparer<TId>.Default.Equals(e.Id, id));
        }

        public NameEntry<TId, TName, TValue>? GetByName(TName name)
        {
            return entries.FirstOrDefault(e => EqualityComparer<TName>.Default.Equals(e.Name, name));
        }

        public List<NameEntry<TId, TName, TValue>> GetAll()
        {
            return entries;
        }
    }
}
