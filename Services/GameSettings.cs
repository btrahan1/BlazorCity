using System;
using System.Collections.Generic;

namespace BlazorCity.Services
{
    public class GameSettings
    {
        // Core
        public int GridSize { get; set; } = 20;
        public int TickSpeedMs { get; set; } = 3000;
        
        // Costs & Revenue
        public decimal ResidentialTaxPerEsim { get; set; } = 10m;
        
        public Dictionary<string, decimal> BuildingCosts { get; set; } = new Dictionary<string, decimal>
        {
            { "Road", 10m },
            { "Home", 500m },
            { "Apartment", 2000m },
            { "Gas", 1000m },
            { "Food", 1500m },
            { "Shop", 1200m },
            { "Arcade", 2500m },
            { "Mall", 10000m },
            { "Bank", 5000m },
            { "Police", 5000m },
            { "Fire", 5000m },
            { "Medical", 8000m },
            { "Park", 500m }
        };

        public Dictionary<string, decimal> BuildingRevenue { get; set; } = new Dictionary<string, decimal>
        {
            { "Food", 50m },
            { "Shop", 80m },
            { "Gas", 100m },
            { "Arcade", 150m },
            { "Bank", 300m },
            { "Mall", 1000m }
        };
        
        public Dictionary<string, decimal> BuildingUpkeep { get; set; } = new Dictionary<string, decimal>
        {
            { "Road", 1m },
            { "Police", 100m },
            { "Fire", 100m },
            { "Medical", 150m },
            { "Park", 5m }
        };

        public Dictionary<string, int> BuildingPopulation { get; set; } = new Dictionary<string, int>
        {
            { "Home", 4 },
            { "Apartment", 40 }
        };

        // Helpers for fuzzy matching
        public decimal GetCost(string name) => FindValue(BuildingCosts, name, 100m);
        public decimal GetRevenue(string name) => FindValue(BuildingRevenue, name, 0m);
        public decimal GetUpkeep(string name) => FindValue(BuildingUpkeep, name, 0m);
        public int GetPopulation(string name) => (int)FindValue(BuildingPopulation.ToDictionary(k=>k.Key, v=>(decimal)v.Value), name, 0m);

        private decimal FindValue(Dictionary<string, decimal> dict, string name, decimal defaultValue)
        {
            if (string.IsNullOrEmpty(name)) return defaultValue;
            foreach (var kvp in dict)
            {
                if (name.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
            }
            return defaultValue;
        }

        // For dynamic updates
        public event Action OnChange;
        public void NotifyChanged() => OnChange?.Invoke();
    }
}
