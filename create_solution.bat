@echo off
echo 正在创建 EAP Gateway 解决方案结构...

REM 创建Core项目子目录
mkdir src\EAP.Gateway.Core\Aggregates 2>nul
mkdir src\EAP.Gateway.Core\Aggregates\EquipmentAggregate 2>nul
mkdir src\EAP.Gateway.Core\Aggregates\MessageAggregate 2>nul
mkdir src\EAP.Gateway.Core\Aggregates\AlarmAggregate 2>nul
mkdir src\EAP.Gateway.Core\Entities 2>nul
mkdir src\EAP.Gateway.Core\ValueObjects 2>nul
mkdir src\EAP.Gateway.Core\DomainServices 2>nul
mkdir src\EAP.Gateway.Core\Events 2>nul
mkdir src\EAP.Gateway.Core\Events\Common 2>nul
mkdir src\EAP.Gateway.Core\Events\Equipment 2>nul
mkdir src\EAP.Gateway.Core\Events\Message 2>nul
mkdir src\EAP.Gateway.Core\Events\Data 2>nul
mkdir src\EAP.Gateway.Core\Events\Alarm 2>nul
mkdir src\EAP.Gateway.Core\Events\System 2>nul
mkdir src\EAP.Gateway.Core\Repositories 2>nul
mkdir src\EAP.Gateway.Core\Specifications 2>nul
mkdir src\EAP.Gateway.Core\Extensions 2>nul
mkdir src\EAP.Gateway.Core\Extensions\Recipe 2>nul
mkdir src\EAP.Gateway.Core\Extensions\RemoteControl 2>nul
mkdir src\EAP.Gateway.Core\Exceptions 2>nul
mkdir src\EAP.Gateway.Core\Constants 2>nul

REM 创建Application项目子目录
mkdir src\EAP.Gateway.Application\Services 2>nul
mkdir src\EAP.Gateway.Application\Commands 2>nul
mkdir src\EAP.Gateway.Application\Commands\Equipment 2>nul
mkdir src\EAP.Gateway.Application\Commands\Message 2>nul
mkdir src\EAP.Gateway.Application\Commands\Data 2>nul
mkdir src\EAP.Gateway.Application\Queries 2>nul
mkdir src\EAP.Gateway.Application\Queries\Equipment 2>nul
mkdir src\EAP.Gateway.Application\Queries\Data 2>nul
mkdir src\EAP.Gateway.Application\Queries\Alarm 2>nul
mkdir src\EAP.Gateway.Application\Handlers 2>nul
mkdir src\EAP.Gateway.Application\Handlers\CommandHandlers 2>nul
mkdir src\EAP.Gateway.Application\Handlers\QueryHandlers 2>nul
mkdir src\EAP.Gateway.Application\Handlers\EventHandlers 2>nul
mkdir src\EAP.Gateway.Application\DTOs 2>nul
mkdir src\EAP.Gateway.Application\Mappings 2>nul
mkdir src\EAP.Gateway.Application\Behaviors 2>nul
mkdir src\EAP.Gateway.Application\Validators 2>nul
mkdir src\EAP.Gateway.Application\Validators\CommandValidators 2>nul
mkdir src\EAP.Gateway.Application\Validators\QueryValidators 2>nul

REM 创建Infrastructure项目子目录
mkdir src\EAP.Gateway.Infrastructure\Persistence 2>nul
mkdir src\EAP.Gateway.Infrastructure\Persistence\Contexts 2>nul
mkdir src\EAP.Gateway.Infrastructure\Persistence\Configurations 2>nul
mkdir src\EAP.Gateway.Infrastructure\Persistence\Repositories 2>nul
mkdir src\EAP.Gateway.Infrastructure\Persistence\Migrations 2>nul
mkdir src\EAP.Gateway.Infrastructure\Caching 2>nul
mkdir src\EAP.Gateway.Infrastructure\Messaging 2>nul
mkdir src\EAP.Gateway.Infrastructure\Messaging\Kafka 2>nul
mkdir src\EAP.Gateway.Infrastructure\Messaging\RabbitMQ 2>nul
mkdir src\EAP.Gateway.Infrastructure\Communications 2>nul
mkdir src\EAP.Gateway.Infrastructure\Communications\SecsGem 2>nul
mkdir src\EAP.Gateway.Infrastructure\Communications\Protocols 2>nul
mkdir src\EAP.Gateway.Infrastructure\Configuration 2>nul
mkdir src\EAP.Gateway.Infrastructure\Logging 2>nul
mkdir src\EAP.Gateway.Infrastructure\Monitoring 2>nul
mkdir src\EAP.Gateway.Infrastructure\Monitoring\HealthChecks 2>nul
mkdir src\EAP.Gateway.Infrastructure\Security 2>nul
mkdir src\EAP.Gateway.Infrastructure\Security\Authentication 2>nul
mkdir src\EAP.Gateway.Infrastructure\Security\Authorization 2>nul
mkdir src\EAP.Gateway.Infrastructure\Security\Encryption 2>nul
mkdir src\EAP.Gateway.Infrastructure\External 2>nul
mkdir src\EAP.Gateway.Infrastructure\External\Notifications 2>nul
mkdir src\EAP.Gateway.Infrastructure\External\Integration 2>nul

REM 创建API项目子目录
mkdir src\EAP.Gateway.Api\Controllers 2>nul
mkdir src\EAP.Gateway.Api\Controllers\V1 2>nul
mkdir src\EAP.Gateway.Api\Controllers\Base 2>nul
mkdir src\EAP.Gateway.Api\Middleware 2>nul
mkdir src\EAP.Gateway.Api\Filters 2>nul
mkdir src\EAP.Gateway.Api\Models 2>nul
mkdir src\EAP.Gateway.Api\Models\Requests 2>nul
mkdir src\EAP.Gateway.Api\Models\Responses 2>nul
mkdir src\EAP.Gateway.Api\Models\Common 2>nul
mkdir src\EAP.Gateway.Api\Extensions 2>nul
mkdir src\EAP.Gateway.Api\Configuration 2>nul

REM 创建Worker项目子目录
mkdir src\EAP.Gateway.Worker\Services 2>nul
mkdir src\EAP.Gateway.Worker\Consumers 2>nul
mkdir src\EAP.Gateway.Worker\Configuration 2>nul

REM 创建测试项目子目录
mkdir tests\EAP.Gateway.UnitTests\Core 2>nul
mkdir tests\EAP.Gateway.UnitTests\Core\Entities 2>nul
mkdir tests\EAP.Gateway.UnitTests\Core\ValueObjects 2>nul
mkdir tests\EAP.Gateway.UnitTests\Core\DomainServices 2>nul
mkdir tests\EAP.Gateway.UnitTests\Core\Specifications 2>nul
mkdir tests\EAP.Gateway.UnitTests\Application 2>nul
mkdir tests\EAP.Gateway.UnitTests\Application\Services 2>nul
mkdir tests\EAP.Gateway.UnitTests\Application\Handlers 2>nul
mkdir tests\EAP.Gateway.UnitTests\Application\Behaviors 2>nul
mkdir tests\EAP.Gateway.UnitTests\Infrastructure 2>nul
mkdir tests\EAP.Gateway.UnitTests\Infrastructure\Repositories 2>nul
mkdir tests\EAP.Gateway.UnitTests\Infrastructure\Communications 2>nul
mkdir tests\EAP.Gateway.UnitTests\Infrastructure\Caching 2>nul
mkdir tests\EAP.Gateway.UnitTests\Api 2>nul
mkdir tests\EAP.Gateway.UnitTests\Api\Controllers 2>nul
mkdir tests\EAP.Gateway.UnitTests\TestHelpers 2>nul
mkdir tests\EAP.Gateway.UnitTests\TestHelpers\Builders 2>nul
mkdir tests\EAP.Gateway.UnitTests\TestHelpers\Fixtures 2>nul
mkdir tests\EAP.Gateway.UnitTests\TestHelpers\Mocks 2>nul

mkdir tests\EAP.Gateway.IntegrationTests\Communications 2>nul
mkdir tests\EAP.Gateway.IntegrationTests\Persistence 2>nul
mkdir tests\EAP.Gateway.IntegrationTests\Messaging 2>nul
mkdir tests\EAP.Gateway.IntegrationTests\Api 2>nul
mkdir tests\EAP.Gateway.IntegrationTests\TestInfrastructure 2>nul

mkdir tests\EAP.Gateway.PerformanceTests\LoadTests 2>nul
mkdir tests\EAP.Gateway.PerformanceTests\StressTests 2>nul
mkdir tests\EAP.Gateway.PerformanceTests\EnduranceTests 2>nul

mkdir tests\EAP.Gateway.AcceptanceTests\Features 2>nul
mkdir tests\EAP.Gateway.AcceptanceTests\Scenarios 2>nul

echo 目录结构创建完成!
echo.
echo 接下来请：
echo 1. 复制解决方案文件到根目录
echo 2. 复制各项目的 .csproj 文件到对应目录
echo 3. 复制全局配置文件到根目录
echo 4. 运行 'dotnet restore' 来恢复 NuGet 包
echo.
pause