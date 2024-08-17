using static Vanara.PInvoke.Ole32;

namespace PrimalZed.CloudSync.Shell;
public interface IClassFactoryOf : IClassFactory {
	Type Type { get; }
}
