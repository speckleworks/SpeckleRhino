<template>
  <div>
    <v-card fluid fill-height v-if='accounts.length == 0' class='elevation-0 pa-4'>
      <h4>Hey there!</h4>
      <p>You don't have any speckle accounts yet. Get started by creating a new one!</p>
      <div class='text-xs-center'>
      <v-btn fab class='light-blue' dark @click.native='EventBus.$emit( "show-register" )'><v-icon>add</v-icon></v-btn>
    </div>
    </v-card>
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
import { EventBus } from '../event-bus'

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
    return {
      EventBus: EventBus
    }
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