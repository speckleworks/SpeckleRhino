<template>
  <v-card class='account-content'>
    <v-card-text xxxclass="grey darken-4">
      <v-layout class='pa--'>
        <v-flex xs3>Token:</v-flex>
        <v-flex xs9><code class="grey darken-4 white--text ellipsis">{{ account.apiToken }}</code></v-flex>
      </v-layout>
      <v-layout class='pa--'>
        <v-flex xs3>URL:</v-flex>
        <v-flex xs9><code class="grey darken-4 white--text ellipsis">{{ account.restApi }}</code></v-flex>
      </v-layout>
      <v-btn block flat small color='red' dark @click='deleteDialog=true'>delete</v-btn>
      <v-dialog v-model="deleteDialog" persistent>
        <v-card>
          <v-card-title class="headline">Are you sure?</v-card-title>
          <v-card-text>This will permanently delete this account, and there's no undo button!</v-card-text>
          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn flat @click.native="deleteDialog=false">Cancel</v-btn>
            <v-btn color="red" @click.native="deleteAccount">Delete</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </v-card-text>
  </v-card>
</template>
<script>
export default {
  name: 'Account',
  props: {
    account: Object,
    expanded: Boolean
  },
  data( ) {
    return {
      deleteDialog: false,
      pinged: false,

    }
  },
  methods: {
    deleteAccount( ) {
      Interop.removeAccount( this.account.fileName )
      this.$store.commit( 'DELETE_ACCOUNT', this.account.fileName )
    }
  },
  mounted () {
    console.log("Yo mounted an account thing", this.account.serverName )
  }
}
</script>
<style lang="scss">
</style>