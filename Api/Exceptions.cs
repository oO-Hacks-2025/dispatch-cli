namespace EmergencyDispatcher.Api;

public class EmptyQueueException(string message) : Exception(message);

public class ValidationException(string message) : Exception(message);
