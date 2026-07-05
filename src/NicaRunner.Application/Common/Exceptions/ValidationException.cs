namespace NicaRunner.Application.Common.Exceptions;

// Reglas de negocio que no pueden expresarse con DataAnnotations de un solo campo
// (ej. EdadMinima < EdadMaxima, edad del corredor fuera del rango de su categoría).
public class ValidationException(string message) : Exception(message);
