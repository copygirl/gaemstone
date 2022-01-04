using System;

namespace gaemstone.Common
{
	/// <summary> Thrown when an existing entity could not be found. </summary>
	public class EntityNotFoundException : Exception
	{
		public object Entity { get; }

		static string BuildMessage(object entity, string? message)
		{
			if (message == null) message = "Entity could not be found";
			message += (entity is uint id) ? $"\n0x{id:X}" : "\n" + entity;
			return message;
		}

		public EntityNotFoundException(object entity)
			: this(entity, null, null) {  }
		public EntityNotFoundException(object entity, string? message)
			: this(entity, message, null) {  }
		public EntityNotFoundException(object entity, Exception? innerException)
			: this(entity, null, innerException) {  }
		public EntityNotFoundException(object entity, string? message, Exception? innerException)
			: base(BuildMessage(entity, message), innerException)
				=> Entity = entity;
	}

	/// <summary> Thrown when an entity with a certain ID already exists. </summary>
	public class EntityExistsException : Exception
	{
		public object Entity { get; }

		static string BuildMessage(object entity, string? message)
		{
			if (message == null) message = "Entity already exists";
			message += "\nEntity: " + ((entity is uint id) ? $"0x{id:X}" : entity);
			return message;
		}

		public EntityExistsException(object entity)
			: this(entity, null) {  }
		public EntityExistsException(object entity, string? message)
			: base(BuildMessage(entity, message))
				=> Entity = entity;
	}

	/// <summary> Thrown when a component or component type could not be found. </summary>
	public class ComponentNotFoundException : Exception
	{
		public object? Entity { get; }
		public object Component { get; }

		static string BuildMessage(object? entity, object component, string? message)
		{
			if (message == null) message = "Component could not be found";
			message += "\nComponent: " + component;
			if (entity != null) message += "\nEntity: " + ((entity is uint id) ? $"0x{id:X}" : entity);
			return message;
		}

		public ComponentNotFoundException(object component)
			: this(null, component, null) {  }
		public ComponentNotFoundException(object? entity, object component)
			: this(entity, component, null) {  }
		public ComponentNotFoundException(object component, string? message)
			: this(null, component, message) {  }
		public ComponentNotFoundException(object? entity, object component, string? message)
			: base(BuildMessage(entity, component, message))
				{ Entity = entity; Component = component; }
	}
}
