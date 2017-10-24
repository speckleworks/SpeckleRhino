<template>
  <div>
    {{account.email}} | {{account.serverName}}
  </div>
</template>

<script>
export default {
  name: 'Account',
  props: {
    account: Object
  },
  data() {
    return {
      accounts: null
    }
  },
  methods: {
    getUserAccounts() {
      Interop.getUserAccounts()
      .then( res => {
        console.log( 'got accounts')
        console.log( res ) 
        this.accounts = JSON.parse( res )
      })
      .catch( err => {
      })
    }
  }
}
</script>

<style scoped>
</style>