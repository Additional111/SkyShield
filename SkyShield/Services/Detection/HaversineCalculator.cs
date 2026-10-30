namespace SkyShield.Services.Detection;

public static class HaversineCalculator
{
    private const double EarthRadiusKm = 6371.0;

    public static double CalculateDistance(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        double lat1 = ToRadians(latitude1);
        double lat2 = ToRadians(latitude2);

        double deltaLat = ToRadians(latitude2 - latitude1);
        double deltaLon = ToRadians(longitude2 - longitude1);

        double a =
            Math.Pow(Math.Sin(deltaLat / 2), 2) +
            Math.Cos(lat1) *
            Math.Cos(lat2) *
            Math.Pow(Math.Sin(deltaLon / 2), 2);

        double c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}