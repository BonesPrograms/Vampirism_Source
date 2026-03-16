using VampirismSys.Serialization;
using XRL.World;
using System;
using System.Reflection;
using XRL.World.Parts;

namespace VampirismSys.Serialization
{
    public static class ScribedWriter
    {
        public static void WriteNamedInstanceFields(SerializationWriter writer, IComposite instance)
        {
            WriteInheritanceLoop(writer, instance, null);
        }



        public static void WriteNamedInstanceFields(SerializationWriter writer, IPart instance)
        {
            WriteInheritanceLoop(writer, instance, typeof(IPart));
        }



        public static void WriteNamedInstanceFields(SerializationWriter writer, Effect instance)
        {
            WriteInheritanceLoop(writer, instance, typeof(Effect));
        }

        static void WriteInheritanceLoop(SerializationWriter writer, object instance, Type limit)
        {
            Type type = instance.GetType();
            while (type != limit)
            {
                WriteClass(writer, instance, type);
                type = type.BaseType;
            }
        }

        static void WriteClass(SerializationWriter writer, object instance, Type type, BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
        {
            FieldInfo[] array = type.GetFields(Flags);
            int size = array.Length;
            int serializeableCount = 0;
            for (int i = 0; i < size; i++)
            {
                if (!array[i].Attributes.HasFlag(FieldAttributes.NotSerialized))
                    serializeableCount++;
            }
            writer.WriteOptimized(serializeableCount);
            for (int x = 0; x < size; x++)
            {
                if (serializeableCount <= 0)
                    break;
                var info = array[x];
                if (!array[x].Attributes.HasFlag(FieldAttributes.NotSerialized))
                {
                    writer.WriteOptimized(info.Name);
                    writer.WriteObject(info.GetValue(instance));
                    serializeableCount--;
                }
            }
        }


    }

    public static class ScribedReader
    {
        public static void ReadNamedInstanceFields(SerializationReader reader, IComposite instance)
        {
            ReadInheritanceLoop(reader, instance, null);
        }
        public static void ReadNamedInstanceFields(SerializationReader reader, IPart instance)
        {
            ReadInheritanceLoop(reader, instance, typeof(IPart));
        }

        public static void ReadNamedInstanceFields(SerializationReader reader, Effect instance)
        {
            ReadInheritanceLoop(reader, instance, typeof(Effect));
        }

        static void ReadInheritanceLoop(SerializationReader reader, object instance, Type limit)
        {
            Type type = instance.GetType();
            while (type != limit)
            {
                ReadClass(reader, instance, type);
                type = type.BaseType;
            }
        }
        static void ReadClass(SerializationReader reader, object instance, Type type, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
        {
            if (instance is BaseVampireSpell part)
            {
                MetricsManager.LogInfo($"{part.ParentObject}");
            }
            FieldInfo[] fields = type.GetFields(flags);
            int serializedCount = reader.ReadOptimizedInt32();
            for (int i = 0; i < serializedCount; i++)
            {
                string serializedName = reader.ReadOptimizedString();
                object serializedValue = reader.ReadObject();
                for (int x = 0; x < fields.Length; x++)
                {
                    FieldInfo field = fields[x];
                    if (field.Name == serializedName)
                    {
                        ReadDebug(type, serializedValue, serializedName);
                        field.SetValue(instance, serializedValue);
                        break;
                    }
                }
            }
        }
        static void ReadDebug(Type type, object value, string name)
        {
            if (type.BaseType == typeof(BasePolymorphSpell) || type.BaseType == typeof(BaseVampireSpell) || type == typeof(BasePolymorphSpell) || type == typeof(BaseVampireSpell))
            {
                MetricsManager.LogInfo($"{value} {name}");
            }
        }
    }



    //These are custom types that use my custom serializer. Inherit from them if you want access to easy serialization of public and private fields.
    //For info on constraints, limitations and RULES!!! see the method definitions.
    //Because there are constraints, limitations and rules

    [Serializable]
    public abstract class IScribedComposite : IComposite
    {
        public bool WantFieldReflection => false;
        public void Write(SerializationWriter Writer)
        {
            ScribedWriter.WriteNamedInstanceFields(Writer, this);
        }

        public void Read(SerializationReader Reader)
        {
            ScribedReader.ReadNamedInstanceFields(Reader, this);
        }
    }

}


namespace XRL.World.Parts
{

    [Serializable]
    public abstract class IBeastScribedPart : IPart
    {
        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            ScribedWriter.WriteNamedInstanceFields(Writer, this);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            ScribedReader.ReadNamedInstanceFields(Reader, this);
        }
    }
}

namespace XRL.World.Effects
{

    [Serializable]
    public abstract class IBeastScribedEffect : Effect
    {
        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            ScribedWriter.WriteNamedInstanceFields(Writer, this);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            ScribedReader.ReadNamedInstanceFields(Reader, this);
        }

    }
}

//INFO:

//ScribedWriter and ScribedReader will read/write from the current type up to it's most base type, halting at the built-in game types (IPart and Effect respectively)
//Public and Private instance fields will be written and read. Mark a field as [NonSerialized] to exclude it.
//Following IScribed rules, fields are serialized and deserialized by name, and you cannot change a field's type without changing it's name.
//If you're inheriting one of my base types, they will have this form of serialization.

//RULES FOR INHERITING FROM IBeastScribed TYPES:

//If you want to serialize a field of a type that cannot be written normally by WriteObject
//you should have that field's type inherit from IScribedComposite, a basetype i made which has the serialization overrides set up for you
//If you have a field that cannot be serialized by WriteObject and you cannot control it's inheritance,
//you should mark it as [NonSerialized], and you will have to serialize it manually (make sure to serialize the name too!) by overriding Read and Write
//If you do not mark it as [NonSerialized], there may be unexpected deserialization problems
//in your overrides, call base.Read and base.Write. 
//Do not invoke any of the ScribedReader or ScribedWriter methods.

//(known) LIMITATIONS:

//ScribedWriter methods cannot serialize GameObjectReference fields (Writer.WriteObject ignores gameobjectreference fields)
//ScribedWriter can only serialize enums whos underlying type is int or uint (Writer.WriteObject handles this)

//EXCEPTIONS:

//Sometimes, uninitialized fields will throw exceptions on deserialization. If you are getting deserialization exceptions
//the first thing you should do is initialize your fields to a non-null value, or mark them [NonSerialized]
//I have not determined the cause of this problem
