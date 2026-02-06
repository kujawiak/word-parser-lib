using ModelDto;

namespace WordParserLibrary.Services.Parsing
{
	/// <summary>
	/// Rejestruje ostrzezenia i komunikaty walidacji na encjach DTO.
	/// </summary>
	public static class ValidationReporter
	{
		public static void AddClassificationWarning(BaseEntity entity, ClassificationResult classification, string expectedType)
		{
			if (!classification.UsedFallback)
			{
				return;
			}

			if (classification.StyleType == null)
			{
				AddValidationMessage(entity, ValidationLevel.Warning,
					$"Brak stylu {expectedType}; uzyto reguly tekstowej.");
				return;
			}

			if (classification.StyleTextConflict)
			{
				AddValidationMessage(entity, ValidationLevel.Warning,
					$"Styl {classification.StyleType} w konflikcie z trescia ({expectedType}); uzyto tresci.");
			}
		}

		public static void AddValidationMessage(BaseEntity entity, ValidationLevel level, string message)
		{
			entity.ValidationMessages.Add(new ValidationMessage(level, message));
		}
	}
}
