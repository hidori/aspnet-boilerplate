FROM mcr.microsoft.com/dotnet/sdk:10.0 AS runtime

ENV HOME=/home/app \
    DOTNET_CLI_HOME=/home/app \
    NUGET_PACKAGES=/home/app/.nuget/packages

RUN mkdir -p /home/app/.nuget/packages /workspaces/aspnet-boilerplate/.artifacts \
    && chown -R app:app /home/app/.nuget /workspaces/aspnet-boilerplate/.artifacts \
    && usermod --shell /bin/bash app

COPY .devcontainer/init-user.sh /usr/local/bin/init-user.sh

WORKDIR /workspaces/aspnet-boilerplate

ENTRYPOINT ["sh", "/usr/local/bin/init-user.sh"]

FROM runtime AS dev

RUN apt-get update \
    && apt-get install -y --no-install-recommends less make nano openssh-client sudo vim \
    && rm -rf /var/lib/apt/lists \
    && printf 'app ALL=(ALL:ALL) NOPASSWD: ALL\n' > /etc/sudoers.d/app \
    && chmod 0440 /etc/sudoers.d/app \
    && visudo -cf /etc/sudoers.d/app
