namespace SkyShield.Attacker.Cli.Attacks;

public interface IAttack
{
    Task ExecuteAsync(string icao);
}