namespace KafkaConsumer.Services;

public class ProductCreationException(string message, Exception? inner = null) : Exception(message, inner);
public class PriceTierCreationException(string message, Exception? inner = null) : Exception(message, inner);
public class ProductUpdateException(string message, Exception? inner = null) : Exception(message, inner);
public class ProductDeleteException(string message, Exception? inner = null) : Exception(message, inner);
public class PriceTierUpdateException(string message, Exception? inner = null) : Exception(message, inner);
public class PriceTierDeleteException(string message, Exception? inner = null) : Exception(message, inner);
