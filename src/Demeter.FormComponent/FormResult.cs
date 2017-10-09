using System.Collections.Generic;

namespace Demeter.FormComponent
{
    public class FormResult
    {
        public FormResult() { }

        static FormResult()
        {
            FormResult.Success = new FormResult();
            FormResult.Success.Succeeded = true;
        }

        public static FormResult Success { get; private set; }

        public bool Succeeded { get; private set; }

        public IEnumerable<FormError> Errors { get; private set; }

        public static FormResult Failed(params FormError[] errors)
        {
            var result = new FormResult();
            result.Succeeded = false;
            result.Errors = errors;
            return result;
        }

        public override string ToString()
        {
            return "";
        }
    }
}