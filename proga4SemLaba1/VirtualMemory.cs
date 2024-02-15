

using System.Collections;

namespace proga4SemLaba1;

public class VirtualMemory
{
    private readonly Page[] _bufferPages;
    public byte PageSize { get; } = 127; //размер страницы в байтах
    private readonly string _fileName;
    private readonly FileStream _fileStream;

    public VirtualMemory(int pageCount, long size, string fileName = "file.bin")
    {
        _fileName = fileName;
        _bufferPages = new Page[pageCount];
        if (_bufferPages == null) throw new ArgumentNullException(nameof(_bufferPages));
        var path = @"C:\Users\batal\RiderProjects\proga4Sem\proga4Sem\bin\Debug\net7.0\" + fileName;
        var drive = new DriveInfo(Path.GetPathRoot(path) ?? throw new InvalidOperationException());
        if (drive.AvailableFreeSpace < size)
        {
            Console.WriteLine("Недостаточно места на диске для размещения файла.");
            return;
        }
        try
        {
            if (!File.Exists(_fileName))
            {
                _fileStream = new FileStream(_fileName, FileMode.Create, FileAccess.ReadWrite);
                //запись сигнатуры
                var signature = "VM"u8.ToArray();
                _fileStream.Seek(0, SeekOrigin.Begin);
                _fileStream.Write(signature, 0, signature.Length);
                //заполнение нулями
                var emptyPage = new byte[size];
                for (var i = 0; i < size; i++)
                {
                    _fileStream.Write(emptyPage, 0, emptyPage.Length);
                }
            }
            else _fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.ReadWrite);

            if (pageCount < 3) throw new ArgumentException("Количество страниц должно быть больше 2");
            {
                for (var i = 0; i < pageCount; i++)
                {
                    var page = new Page
                    {
                        PageNumber = i,
                        Status = 0,
                        WriteTime = DateTime.Now,
                        BitMap = new BitArray((PageSize * sizeof(int) + sizeof(int)) / 8),
                        Data = new int[(PageSize * sizeof(int) + sizeof(int)) / sizeof(int)]
                    };
                    for (var j = 0; j < page.Data.Length; j++)
                    {
                        page.Data[j] = 0;
                    }

                    for (var k = 0; k < page.BitMap.Length; k++)
                    {
                        page.BitMap[k] = false;
                    }

                    _bufferPages[i] = page;
                }
                
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine("Произошла ошибка при записи файла: " + ex.Message);
        }
        catch (OutOfMemoryException ex)
        {
            Console.WriteLine("Произошла ошибка из-за недостатка оперативной памяти: " + ex.Message);
        }
        catch (IndexOutOfRangeException ex)
        {
            Console.WriteLine("Произошла ошибка: индекс выходит за границы массива. " + ex.Message);
        }
    }
    public bool WriteArrayElement(int index, int value)
    {
        var pageNumber = FindPageNum(index);
        var pageOffset = index % (PageSize*sizeof(int) + sizeof(int)); // Вычисляем смещение внутри страницы
        // Поиск страницы в буфере
        try
        {
            var pageIndex = -1;
            for (var i = 0; i < _bufferPages.Length; i++)
            {
                if (_bufferPages[i].PageNumber == pageNumber)
                {
                    pageIndex = i;
                    break;
                }
            }

            // Если страницы нет в буфере, операция завершается неудачно
            if (pageIndex == -1) return false;
            // Модификация атрибутов страницы
            _bufferPages[pageIndex].Status = 1;
            _bufferPages[pageIndex].WriteTime = DateTime.Now;
            _bufferPages[pageIndex].BitMap[pageOffset / 8] = true;
            _bufferPages[pageIndex].Data[pageOffset] = value;
            foreach (var page in _bufferPages)
            {
                _fileStream.Seek(index, SeekOrigin.Begin);
                _fileStream.Write(BitConverter.GetBytes(value), 0, sizeof(byte));
                _fileStream.Write(BitConverter.GetBytes(true), 0, sizeof(bool));
            }
        }
        catch (IndexOutOfRangeException ex)
        {
            Console.WriteLine("Произошла ошибка: индекс выходит за границы массива. " + ex.Message);
        }

        return true;
    }

    //Метод определения номера (индекса) страницы в буфере страниц, где находится элемент массива с заданным индексом
    public int? FindPageNum(int index)
    {
        var pageNumber = index / (PageSize*sizeof(int) + sizeof(int)); // Вычисляем абсолютный номер страницы
        var oldestPageIndex = 0;
        try
        {

            //Проверяем наличие страницы в буфере
            for (var i = 0; i < _bufferPages.Length; i++)
            {
                var bufferPage = _bufferPages[i];
                if (bufferPage.PageNumber != pageNumber) continue;
                bufferPage.WriteTime = DateTime.Now;
                bufferPage.Status = 1; // Устанавливаем флаг модификации
                return i; // Возвращаем индекс страницы в буфере
            }

            var oldestWriteTime = _bufferPages[0].WriteTime;
            for (var i = 1; i < _bufferPages.Length; i++)
            {
                if (_bufferPages[i].WriteTime >= oldestWriteTime) continue;
                oldestWriteTime = _bufferPages[i].WriteTime;
                oldestPageIndex = i;
            }

            // Проверяем флаг модификации выбранной страницы и выгружаем ее при необходимости
            if (_bufferPages[oldestPageIndex].Status == 1)
            {
                var pageData = _bufferPages[pageNumber].Data;
                var pageBit = _bufferPages[pageNumber].BitMap;
                var offset = _bufferPages[pageNumber].PageNumber * PageSize;
                _fileStream.Seek(offset, SeekOrigin.Begin);
                foreach (var data in pageData)
                {
                    _fileStream.Write(BitConverter.GetBytes(data), 0, sizeof(int));
                }

                foreach (var bit in pageBit)
                {
                    _fileStream.Write(BitConverter.GetBytes(false), 0, sizeof(bool));
                }

                _bufferPages[oldestPageIndex].Status = 0;
            }

            // Загружаем новую страницу в буфер
            var newPage = new Page
            {
                PageNumber = pageNumber,
                Status = 0,
                WriteTime = DateTime.Now,
                BitMap = new BitArray((PageSize * sizeof(int) + sizeof(int) / 8)),
                Data = new int[(PageSize * sizeof(int) + sizeof(int))]
            };
            _bufferPages[oldestPageIndex] = newPage;
        }
        catch (IndexOutOfRangeException ex)
        {
            Console.WriteLine("Произошла ошибка: индекс выходит за границы массива. " + ex.Message);
        }
        return oldestPageIndex; // Возвращаем индекс новой страницы в буфере
    }
    //Метод чтения значения элемента массива с заданным индексом в указанную переменную
    public bool ReadArrayElement(int index, out int value)
    {
        var pageNumber = FindPageNum(index);//номер страницы
        var pageOffset = index % (PageSize / sizeof(int)); // Вычисляем смещение внутри страницы
        try
        {
        // Поиск страницы в буфере
        var pageIndex = -1;
        for (var i = 0; i < _bufferPages.Length; i++)
        {
            if (_bufferPages[i].PageNumber != pageNumber) continue;
            pageIndex = i;
            break;
        }
        // Если страницы нет в буфере, операция завершается неудачно
        if (pageIndex == -1)
        { 
            value = 0; // Устанавливаем значение по умолчанию
            return false;
        }
        // Копирование значения из буфера в указанную переменную
        value = _bufferPages[pageIndex].Data[pageOffset];
        }
        catch (IndexOutOfRangeException ex)
        {
            Console.WriteLine("Произошла ошибка: индекс выходит за границы массива. " + ex.Message);
        }
        value = 0;
        return true; // Операция завершена успешно
    }
    public Page[] ArrayOfPages()
    {
        return _bufferPages;
    }
}