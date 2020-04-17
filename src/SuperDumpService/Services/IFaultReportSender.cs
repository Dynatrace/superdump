using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public interface IFaultReportSender {
		Task SendFaultReport(FaultReport faultReport);
	}
}