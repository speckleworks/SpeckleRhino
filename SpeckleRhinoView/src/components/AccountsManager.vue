<template>
  <div>
    <v-expansion-panel>
      <v-expansion-panel-content v-for='account in accounts' :key="account.apiToken" class='account'>
        <div slot='header'>
          <div class="subheading">{{account.serverName}}</div>
          <div class='caption'>{{account.email}}</div>
        </div>
        <account :account='account'></account>
      </v-expansion-panel-content>
    </v-expansion-panel>
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
    accounts( ) {
      return this.$store.getters.accounts
    }
  },
  data( ) {
    return {}
  },
  methods: {
    getUserAccounts( ) {}
  },
  mounted( ) {}
}
</script>
<style scoped>
.account {
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>