using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;

namespace MF.Radius.Core.Packets.Parsing;

/// <summary>
/// Обеспечивает перечисление атрибутов RADIUS-пакета в памяти без аллокаций в куче.
/// Реализует паттерн итератора (duck typing), что позволяет использовать структуру в цикле foreach.
/// </summary>
/// <param name="data">Срез памяти (Span), содержащий только блок атрибутов RADIUS-пакета.</param>
public ref struct RadiusAttributeEnumerator(
    ReadOnlyMemory<byte> data
)
    // ref struct не может реализовывать интерфейсы!!!
    // : IEnumerator<RadiusAttribute>
{
    private ReadOnlyMemory<byte> _data = data;

    // Возвращает элемент, на котором мы сейчас стоим.
    // Должно возвращать объект или другую ref struct.
    public RadiusAttribute Current { get; private set; } = default;

    // Продвигает курсор на один шаг вперед.
    // Возвращает true, если шаг удался, и false, если мы дошли до конца.
    public bool MoveNext()
    {
        if (_data.Length < 2) return false;

        var type = _data.Span[0];
        var length = _data.Span[1];

        if (length < 2 || length > _data.Length)
            throw new InvalidOperationException("Invalid attribute length");
        
        // ВАЖНО: RadiusAttribute тоже должен быть ref struct, 
        // чтобы хранить Span внутри Value без копирования
        Current = new RadiusAttribute
        {
            Type = (RadiusAttributeType)type,
            Length = length,
            Value = _data[2..length]
        };

        // Сдвигаем окно на следующий атрибут
        _data = _data[length..];
        return true;
        
    }
    
    // Сбрасывает курсор в начало (до первого элемента).
    // Обычно используется редко.
    // public void Reset() { }
    //
    // public void Dispose() { }

    // Этот метод позволяет писать
    // foreach (var attr in new RadiusAttributeEnumerator(data))
    public RadiusAttributeEnumerator GetEnumerator() => this;
    
}
