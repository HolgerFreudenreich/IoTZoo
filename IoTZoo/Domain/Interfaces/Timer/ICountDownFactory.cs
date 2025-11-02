using Domain.Pocos;
using Domain.Services.Timer;

namespace Domain.Interfaces.Timer
{
    public interface ICountDownFactory
    {
        CountDownService Create(CountDownData countDownData);
    }
}