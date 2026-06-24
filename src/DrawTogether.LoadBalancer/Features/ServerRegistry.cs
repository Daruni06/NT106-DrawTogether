using System.Collections.Generic;

namespace DrawTogether.LoadBalancer.Features
{
    public class ServerRegistry
    {
        private readonly List<string> _servers = new();

        public void Register(string server)
        {
            if (!_servers.Contains(server))
            {
                _servers.Add(server);
            }
        }

        public void Remove(string server)
        {
            _servers.Remove(server);
        }

        public List<string> GetAll()
        {
            return new List<string>(_servers);
        }

        public int Count => _servers.Count;
    }
}