<template>
  <div>
    <md-button class='md-accent' @click.native='getUserAccounts'>Get Accounts</md-button>
    <account 
      :account='ac'
      v-for='ac in accounts'
    ></account>
  </div>
</template>

<script>
import Account from './Account.vue'

export default {
  name: 'AccountsManager',
  components: {
    Account
  },
  computed: {
    accounts() { 
      return this.$store.getters.accounts
    }
  },
  data() {
    return {
    }
  },
  methods: {
    getUserAccounts() {
      Interop.getUserAccounts()
      .then( res => {
        this.$store.commit( 'SET_ACCOUNTS', JSON.parse( res ) )
      })
      .catch( err => {
      })
    }
  },
  mounted () {
  }
}
</script>

<style scoped>
</style>