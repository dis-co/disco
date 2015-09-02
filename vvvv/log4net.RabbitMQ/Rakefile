require 'bundler/setup'

require 'albacore'
require 'albacore/tasks/versionizer'
require 'albacore/tasks/release'

Albacore::Tasks::Versionizer.new :versioning

task :paket do
  system 'src/.paket/paket.bootstrapper.exe', clr_command: true unless
    File.exists? 'src/paket/paket.exe'
end

desc 'restore all nuget pkgs'
task :restore => :paket do |r|
  system 'src/.paket/paket.exe', %w|install|, clr_command:true
end

desc 'build the solution'
build :build => [:versioning, :restore] do |b|
  b.prop 'Configuration', 'Release'
  b.sln = 'src/log4net.RabbitMQ.sln'
end

directory 'build/pkg'

nugets_pack :create_nugets => ['build/pkg', :versioning, :build] do |p|
  p.files   = FileList['src/log4net.RabbitMQ/*.csproj']
  p.out     = 'build/pkg'
  p.exe     = 'packages/NuGet.CommandLine/tools/NuGet.exe'
  p.with_metadata do |m|
    m.id = "log4net.RabbitMQAppender"
    m.description = 'Log4net appender for RabbitMQ'
    m.authors = 'Henrik Feldt'
    m.project_url = 'https://github.com/haf/log4net.RabbitMQ'
    m.version = ENV['NUGET_VERSION']
    m.tags = 'rabbitmq log4net'
  end
end

Albacore::Tasks::Release.new :release,
                             pkg_dir: 'build/pkg',
                             depend_on: :create_nugets,
                             nuget_exe: 'packages/NuGet.CommandLine/tools/NuGet.exe',
                             api_key: ENV['NUGET_KEY']
desc 'runs create_nugets'
task :default => :create_nugets
