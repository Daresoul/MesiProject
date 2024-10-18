using System;
using System.Collections.Generic;

namespace Client
{
    public class SaveUiElement
    {
        public Guid Id { get; set; }
        public string TypeName { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
        public string? Value3 { get; set; }
        public List<Guid>? Contains { get; set; }
        public Guid? Next { get; set; }
        public Guid? NextOf { get; set; }
        public Guid? PartOf { get; set; }
        
        public SaveUiElement()
        {
        }

        public SaveUiElement(Guid id, Type type, double positionX, double positionY, BaseRect? next, BaseRect? nextOf, BaseContainer? partOf, string? value1, string? value2, string? value3, List<Guid> contains)
        {
            Id = id;
            TypeName = type?.FullName ?? throw new ArgumentNullException(nameof(type), "Type cannot be null");
            PositionX = positionX;
            PositionY = positionY;
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
            Contains = contains;
            Next = next?.Id;
            NextOf = nextOf?.Id;
            PartOf = partOf?.Id;
        }
        public Type GetElementType()
        {
            if (string.IsNullOrEmpty(TypeName))
            {
                throw new ArgumentNullException(nameof(TypeName), "TypeName is null or empty.");
            }

            var type = Type.GetType(TypeName);
            if (type == null)
            {
                throw new ArgumentException($"Cannot find type {TypeName}");
            }

            return type;
        }
    }
}