namespace ServerlessAPI.Validators;

public class ValidationFailedException(string message) : Exception(message);