using System;

namespace BlazorCity.Services
{
    public class CityState
    {
        // Economy
        public decimal Funds { get; private set; } = 20000m;
        public decimal TaxRate { get; set; } = 0.10m; // 10%
        public decimal NetIncome { get; private set; } = 0m;

        // Spatial Tracking
        public class BuildingInstance
        {
            public string Name { get; set; } = "";
            public int X { get; set; }
            public int Y { get; set; }
            public bool IsConnected { get; set; }
        }

        public List<BuildingInstance> PlacedBuildings { get; private set; } = new List<BuildingInstance>();
        private HashSet<(int, int)> _roadTiles = new HashSet<(int, int)>();

        // Population
        public int Population { get; private set; } = 0;
        public int Jobs { get; private set; } = 0;

        // Time
        public DateTime GameTime { get; private set; } = new DateTime(2026, 1, 1);
        private System.Timers.Timer _gameTimer;
        
        // Events
        public event Action OnChange;

        public CityState()
        {
            // Tick every 3 seconds (1 Game Day)
            _gameTimer = new System.Timers.Timer(3000);
            _gameTimer.Elapsed += (s, e) => Tick();
            _gameTimer.AutoReset = true;
            _gameTimer.Start();
        }

        public void RegisterBuilding(string name, int x, int y)
        {
            // Track Building
            var b = new BuildingInstance { Name = name, X = x, Y = y, IsConnected = false };
            PlacedBuildings.Add(b);

            // Track Road specifically for O(1) lookups
            if (name.Contains("Road"))
            {
                _roadTiles.Add((x, y));
            }
            
            RecalculateConnectivity();
            RecalculateDemographics();
            NotifyStateChanged();
        }

        public void DeductFunds(decimal amount)
        {
            if (Funds >= amount)
            {
                Funds -= amount;
                NotifyStateChanged();
            }
            else
            {
                throw new InvalidOperationException("Insufficient funds.");
            }
        }

        public void AddFunds(decimal amount)
        {
            Funds += amount;
            NotifyStateChanged();
        }

        private void RecalculateConnectivity()
        {
            // Simple Logic: adjacent to any road tile?
            // (Assuming 1x1 size for now for simplicity, or anchor point proximity)
            foreach (var b in PlacedBuildings)
            {
                // Roads are always connected
                if (b.Name.Contains("Road")) 
                {
                    b.IsConnected = true;
                    continue;
                }

                // Check Neighbors (N, S, E, W)
                bool connected = _roadTiles.Contains((b.X + 1, b.Y)) ||
                                 _roadTiles.Contains((b.X - 1, b.Y)) ||
                                 _roadTiles.Contains((b.X, b.Y + 1)) ||
                                 _roadTiles.Contains((b.X, b.Y - 1));
                
                b.IsConnected = connected;
            }
        }

        private void RecalculateDemographics()
        {
            int pop = 0;
            decimal commercialTax = 0;
            decimal upkeep = 0;

            foreach(var b in PlacedBuildings)
            {
                // Upkeep is paid regardless of connectivity!
                if (b.Name.Contains("Road")) upkeep += 1;
                if (b.Name.Contains("Police")) upkeep += 100;
                if (b.Name.Contains("Medical") || b.Name.Contains("Medic")) upkeep += 150;
                if (b.Name.Contains("Fire")) upkeep += 100;
                if (b.Name.Contains("Park")) upkeep += 5;

                // Income/Pop only if connected
                if (!b.IsConnected) continue;

                // Residential
                if (b.Name.Contains("Home")) pop += 4;
                if (b.Name.Contains("Apartment")) pop += 40;

                // Commercial
                if (b.Name.Contains("Food") || b.Name.Contains("Restaurant")) commercialTax += 50;
                if (b.Name.Contains("Shop") || b.Name.Contains("Store")) commercialTax += 80;
                if (b.Name.Contains("Gas")) commercialTax += 100;
                if (b.Name.Contains("Arcade")) commercialTax += 150;
                if (b.Name.Contains("Bank")) commercialTax += 300;
                if (b.Name.Contains("Mall")) commercialTax += 1000;
            }

            Population = pop;
            decimal residentialTax = Population * 10 * TaxRate; 
            decimal totalIncome = residentialTax + commercialTax;

            NetIncome = totalIncome - upkeep;
        }

        public void Tick()
        {
            // Advance Time (1 Day per Tick)
            GameTime = GameTime.AddDays(1);

            // Monthly Logic (Every 30th approx, simplifed to Day 1)
            if (GameTime.Day == 1)
            {
                ProcessMonthlyBudget();
            }

            NotifyStateChanged();
        }

        private void ProcessMonthlyBudget()
        {
            if (NetIncome >= 0) AddFunds(NetIncome);
            else DeductFunds(Math.Abs(NetIncome));
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
