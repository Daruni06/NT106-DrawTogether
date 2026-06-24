using System.Collections.Generic;

namespace DrawTogether.LoadBalancer.Features
{
    public class LoadBalancingService
    {
        private int _currentIndex = 0;

        public string? GetNextServer(List<string> servers)
        {
            if (servers.Count == 0)
                return null;

            string server = servers[_currentIndex];

            _currentIndex =
                (_currentIndex + 1) % servers.Count;

            return server;
        }
    }
}