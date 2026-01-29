using Microsoft.JSInterop;

namespace BlazorCity.Services
{
    public class EconomyInterop
    {
        private readonly CityState _cityState;
        private readonly GameSettings _settings;

        public EconomyInterop(CityState cityState, GameSettings settings)
        {
            _cityState = cityState;
            _settings = settings;
        }

        [JSInvokable]
        public bool TryPurchase(string buildingName, int x, int y)
        {
            decimal cost = _settings.GetCost(buildingName);
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
        public decimal GetBuildingCost(string buildingName) => _settings.GetCost(buildingName);
    }
}
