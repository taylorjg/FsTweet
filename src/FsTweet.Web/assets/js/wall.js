$(function(){
  $("#tweetForm").submit(function(event){
    var $this = $(this);
    var $tweet = $("#tweet");
    event.preventDefault();
    $this.prop('disabled', true);
    $.ajax({
      url : "/tweets",
      type: "post",
      data: JSON.stringify({post : $tweet.val()}),
      contentType: "application/json"
    }).done(function(){
      $this.prop('disabled', false);
      $tweet.val('');
    }).fail(function(jqXHR, textStatus, errorThrown) {
      console.log({jqXHR : jqXHR, textStatus : textStatus, errorThrown: errorThrown})
      alert("something went wrong!")
    });
  });
});
