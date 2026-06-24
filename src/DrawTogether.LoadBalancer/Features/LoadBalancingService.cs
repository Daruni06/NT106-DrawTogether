using System.Collections.Generic;

namespace DrawTogether.LoadBalancer.Features
{
    public class LoadBalancingService
    {
        private int _currentIndex = 0;
        private readonly Dictionary<string, string> _roomMap = new();

        public void RemoveServer(string server)
        {
            var keys = new List<string>();
            foreach (var kv in _roomMap)
            {
                if (kv.Value == server) keys.Add(kv.Key);
            }
            foreach (var k in keys) _roomMap.Remove(k);
        }

        public string? GetNextServer(List<string> servers)
        {
            if (servers.Count == 0)
                return null;

            string server = servers[_currentIndex];

            _currentIndex =
                (_currentIndex + 1) % servers.Count;

            return server;
        }

        public string? GetServerForRoom(string roomId, List<string> servers)
        {
            if (string.IsNullOrWhiteSpace(roomId))
                return GetNextServer(servers);

            if (_roomMap.TryGetValue(roomId, out var existing))
            {
                if (servers.Contains(existing)) return existing;
                // previously assigned server is gone, fall through to assign new
                _roomMap.Remove(roomId);
            }

            var assigned = GetNextServer(servers);
            if (assigned != null)
            {
                _roomMap[roomId] = assigned;
            }
            return assigned;
        }
    }
}