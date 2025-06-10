FROM  stripe/stripe-cli:v1.25.0
RUN  apk  add  pass  gpg-agent
COPY  ./entrypoint.sh  /entrypoint.sh
ENTRYPOINT  [ "/entrypoint.sh" ]
