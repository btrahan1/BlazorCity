using Microsoft.JSInterop;

namespace BlazorCity.Services
{
    public class EconomyInterop
    {
        private readonly CityState _cityState;

        public EconomyInterop(CityState cityState)
        {
            _cityState = cityState;
        }

        [JSInvokable]
        public bool TryPurchase(string buildingName, int x, int y)
        {
            decimal cost = GetCost(buildingName);
            try 
            {
                _cityState.DeductFunds(cost);
                _cityState.RegisterBuilding(buildingName, x, y);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [JSInvokable]
        public decimal GetBuildingCost(string buildingName) => GetCost(buildingName);

        private decimal GetCost(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            if (name.Contains("Road")) return 10;
            if (name.Contains("Home")) return 500;
            if (name.Contains("Apartment")) return 2000;
            if (name.Contains("Gas")) return 1000;
            if (name.Contains("Food") || name.Contains("Restaurant")) return 1500;
            if (name.Contains("Shop") || name.Contains("Store")) return 1200;
            if (name.Contains("Arcade")) return 2500;
            if (name.Contains("Mall")) return 10000;
            if (name.Contains("Bank")) return 5000;
            if (name.Contains("Police")) return 5000;
            if (name.Contains("Fire")) return 5000;
            if (name.Contains("Medical")) return 8000;
            if (name.Contains("Park")) return 500;
            
            return 100; // Default
        }
    }
}
