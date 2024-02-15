using System.Collections;

namespace proga4SemLaba1;


//Структура страницы, находящейся в памяти
public struct Page
{
    public int PageNumber;//абсолютный номер страницы (порядковый номер страницы в файле);
    public byte Status;//статус  страницы  (флаг модификации) – байт, определяющий статус страницы (0 – стра-ница не модифицировалась, 1 – если была запись);
    public DateTime WriteTime;//время записи страницы в память;
    public BitArray BitMap;
    public int[] Data;//массив значений моделируемого массива, находящихся на странице.
}