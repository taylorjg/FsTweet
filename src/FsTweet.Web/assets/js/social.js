$(() => {
  $('#follow').on('click', () => {
    // const $this = $(this)
    const $this = $(event.currentTarget)
    const userId = $this.data('user-id')
    $this.prop('disabled', true)
    $.ajax({
      url : '/follow',
      type: 'post',
      data: JSON.stringify({userId}),
      contentType: 'application/json'
    }).done(() => {
      alert('Successfully followed')
      $this.prop('disabled', false)
    }).fail((jqXHR, textStatus, errorThrown) => {
      console.log({jqXHR, textStatus, errorThrown})
      alert('Something went wrong!')
    })
  })
})
