<template>
  <div>
    <h2 class='md-display-3'>OHAI</h2>
    <pre>{{$data}}</pre>
  </div>
</template>

<script>
export default {
  name: 'AccountsManager',
  data() {
    return {
      accounts: null
    }
  },
  methods: {
  },
  mounted () {
    console.log('AccountsManager mounted')
    Interop.getUserAccounts()
    .then( res => {
      console.log( 'got accounts')
      console.log( res ) 
      this.accounts = res 
    })
    .catch( err => {

    })
  }
}
</script>

<style scoped>
</style>