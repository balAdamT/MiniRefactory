namespace MiniAnalyzers.Example
{
    internal class Person
    {
        public string Name;
        public int Age;
        private int Height;
    }

    internal class Initializer
    {
        public void Main()
        {
            var person = new Person() { Name = "Bálint Ádám" };
        }
    }
}
