using System.Numerics;
namespace DotDice.RandomNumberGenerator
{
    public interface IRandomNumberGenerator<N> where N : INumber<N>
    {
        void SetSeed(int seed); // Sets the seed value for the random number generator.
        int GetSeed(); // Gets the current seed value of the random number generator (if supported).
        N Next(); // Returns a non-negative random Number.
        N Next(N maxValue); // Returns a non-negative random Number less than maxValue.
        N Next(N minValue, N maxValue); // Returns a random Number within the specified range (exclusive).
    }
}