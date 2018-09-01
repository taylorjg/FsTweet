DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
VERSION=`perl -n -e '/<Version>([\d.]+)<\/Version>/ && print $1' "$DIR/src/FsTweet.Web/FsTweet.Web.fsproj"`
sed -i -e s/__VERSION__/$VERSION/ "$1"
