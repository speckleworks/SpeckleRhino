<template>
  <div>
   <!--  <v-layout row wrap style='padding:10px;'>
      <v-flex xs12>
        <account :account='ac' v-for='ac in accounts' :key="ac.apiToken"></account>
      </v-flex>
    </v-layout> -->
    <v-list avatar dark two-line>
      <v-subheader>The following accounts have been found on your computer.</v-subheader>
      <template v-for='ac in accounts' >
        <!-- <account :account='ac' :key='ac.apiToken'></account>   -->
        <v-divider :key='ac.apiToken'></v-divider>
        <v-list-group avatar :value='true'>
          <v-list-tile slot='item' @click=''>
            <v-list-tile-avatar>
              <v-icon class='blue'>person</v-icon>
            </v-list-tile-avatar>
            <v-list-tile-content>
              <v-list-tile-title v-html="ac.serverName"></v-list-tile-title>
              <v-list-tile-sub-title v-html="ac.email"></v-list-tile-sub-title>
            </v-list-tile-content>
            <v-list-tile-action>
              <v-icon>keyboard_arrow_down</v-icon>
            </v-list-tile-action>
          </v-list-tile>
          <v-list-tile>
            <v-list-tile-content>
              <v-list-tile-title>{{ac.restApi}}</v-list-tile-title>
              <v-list-tile-sub-title>{{ac.apiToken}}</v-list-tile-sub-title>
            </v-list-tile-content>
            <v-list-tile-action>
              <v-icon>delete</v-icon>
            </v-list-tile-action>
          </v-list-tile>
        </v-list-group>
      </template>
      <v-btn class='md-accent md-dense md-flat' @click.native='getUserAccounts'>Get Accounts</v-btn>
    </v-list>
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