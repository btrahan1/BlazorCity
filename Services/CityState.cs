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
        
        // Dependencies
        private readonly GameSettings _settings;
        
        // Events
        public event Action OnChange;

        public CityState(GameSettings settings)
        {
            _settings = settings;
            _settings.OnChange += OnSettingsChanged;

            // Tick Timer
            SetupTimer();
        }

        private void OnSettingsChanged()
        {
            if (_gameTimer.Interval != _settings.TickSpeedMs)
            {
                _gameTimer.Interval = _settings.TickSpeedMs;
            }
            RecalculateDemographics();
            NotifyStateChanged();
        }

        private void SetupTimer()
        {
            _gameTimer = new System.Timers.Timer(_settings.TickSpeedMs);
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
                string name = b.Name;
                
                // Expenses (Always paid)
                upkeep += _settings.GetUpkeep(name);

                // Income/Pop (Only if connected)
                if (!b.IsConnected) continue;

                // Population
                pop += _settings.GetPopulation(name);

                // Revenue
                commercialTax += _settings.GetRevenue(name);
            }

            Population = pop;
            decimal residentialTax = Population * _settings.ResidentialTaxPerEsim * TaxRate; 
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

        public void LoadState(CitySaveData data)
        {
            Funds = data.Funds;
            Population = data.Population;
            GameTime = data.Time;
            PlacedBuildings = data.Buildings ?? new List<BuildingInstance>();
            _roadTiles.Clear();
            foreach(var b in PlacedBuildings)
            {
                if (b.Name.Contains("Road")) _roadTiles.Add((b.X, b.Y));
            }
            if (data.Settings != null)
            {
                _settings.GridSize = data.Settings.GridSize;
                _settings.TickSpeedMs = data.Settings.TickSpeedMs;
                _settings.ResidentialTaxPerEsim = data.Settings.ResidentialTaxPerEsim;
                _settings.BuildingCosts = data.Settings.BuildingCosts;
                _settings.BuildingRevenue = data.Settings.BuildingRevenue;
                _settings.NotifyChanged();
            }

            RecalculateConnectivity();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }

    public class CitySaveData
    {
        public decimal Funds { get; set; }
        public int Population { get; set; }
        public DateTime Time { get; set; }
        public List<CityState.BuildingInstance> Buildings { get; set; }
        public GameSettings Settings { get; set; }
    }
}
