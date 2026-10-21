using System.Collections.Generic;
using UnityEngine;
namespace EchoGame
{
    public sealed class ScanRegistry
    {
        readonly Dictionary<string, PhaseNode> nodes = new Dictionary<string, PhaseNode>();
        public void Rebuild(IEnumerable<PhaseNode> source)
        {
            nodes.Clear();
            foreach (var node in source)
            {
                if (!node || string.IsNullOrWhiteSpace(node.StableId) || nodes.ContainsKey(node.StableId))
                    throw new System.InvalidOperationException("ECHO scanner: каждому узлу нужен непустой уникальный StableId.");
                nodes.Add(node.StableId, node);
            }
        }
        public bool TryGet(string id, out PhaseNode node)
        { node = null; return !string.IsNullOrEmpty(id) && nodes.TryGetValue(id, out node) && node; }
    }
}
