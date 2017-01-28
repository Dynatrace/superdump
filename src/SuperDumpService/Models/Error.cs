namespace SuperDumpService.Models {
	public class Error {
		public string ErrorReason { get; set; }
		public string ErrorMessage { get; set; }

		public Error(string reason, string message) {
			this.ErrorReason = reason;
			this.ErrorMessage = message;
		}
	}
}
