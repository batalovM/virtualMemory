using proga4SemLaba1;

class Progarm
{
    public static void Main(string[] args)
    {
        var file = new VirtualMemory(3, 1000, "file.bin");
        var pages = file.ArrayOfPages();
        var arr = new int[(file.PageSize*sizeof(int) + sizeof(int)) / sizeof(int)];
        Console.WriteLine($"Вывод значения элементов массива до записи");
        for (var i = 0; i < pages.Length; i++)
        {
            Console.WriteLine("\nСледующий массив");
            for (var j = 0; j < arr.Length; j++)
            {
                arr = pages[i].Data;
                file.ReadArrayElement(pages[i].Data[i], out _);
            }
            foreach (var t in arr)
            {
                Console.Write($"{t} ");
            }
        }
        file.WriteArrayElement(3, 15);
        file.WriteArrayElement(514, 256);
        file.WriteArrayElement(1026, 64);
        var arr1 = new int[(file.PageSize*sizeof(int) + sizeof(int)) / sizeof(int)];
        Console.WriteLine($"\nВывод значения элементов массива после записи");
        for (var i = 0; i < pages.Length; i++)
        {
            Console.WriteLine("\nСледующий массив");
            for (var j = 0; j < arr.Length; j++)
            {
                arr1 = pages[i].Data;
                file.ReadArrayElement(pages[i].Data[i], out _);
            }
            foreach (var t in arr1)
            {
                Console.Write($"{t} ");
            }
        }
    }
}