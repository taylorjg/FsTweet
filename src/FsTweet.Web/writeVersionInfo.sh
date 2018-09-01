VERSION=`LANG=en_US.UTF-8 perl -n -e '/<Version>([\d.]+)<\/Version>/ && print $1' FsTweet.Web.fsproj`
sed -i -e s/__VERSION__/$VERSION/ "$1"
