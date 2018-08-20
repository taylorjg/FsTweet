$(() => {
  $('#tweetForm').submit(event => {
    const $this = $(this)
    const $tweet = $('#tweet')
    event.preventDefault()
    $this.prop('disabled', true)
    $.ajax({
      url : '/tweets',
      type: 'post',
      data: JSON.stringify({post : $tweet.val()}),
      contentType: 'application/json'
    }).done(() => {
      $this.prop('disabled', false)
      $tweet.val('')
    }).fail((jqXHR, textStatus, errorThrown) => {
      console.log({jqXHR : jqXHR, textStatus : textStatus, errorThrown: errorThrown})
      alert('Something went wrong!')
    })
  })

  const client = stream.connect(fsTweet.stream.apiKey, null, fsTweet.stream.appId)
  const userFeed = client.feed('user', fsTweet.user.id, fsTweet.user.userFeedToken)

  userFeed.subscribe(data => {
    console.dir(data)
    renderTweet($('#wall'), data.new[0])
  })
})
