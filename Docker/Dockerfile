FROM stratisplatform/dotnet-build:1.0.0-preview2-1-003177

MAINTAINER Dan Gershony - Stratis Platform <dan.gershony@stratisplatform.com>

RUN git clone https://github.com/nitro-coin/NitrocoinFullNode.git \
	&& cd  NitrocoinFullNode \
	&& dotnet restore \
	&& cd Nitrocoin.BitcoinD \
	&& dotnet build
	
VOLUME /root/.nitrocoinbitcoin

WORKDIR /NitrocoinFullNode/Nitrocoin.NitrocoinD

EXPOSE 8333 8332 18333 18332

CMD ["dotnet", "run"]
